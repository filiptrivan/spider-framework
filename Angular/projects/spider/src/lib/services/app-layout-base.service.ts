import { AuthBaseService } from './auth-base.service';
import { ApiSecurityService } from './api.service.security';
import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, combineLatest, delay, lastValueFrom, map, Observable, of, Subject, Subscription, withLatestFrom } from 'rxjs';
import { InitTopBarData } from '../entities/init-top-bar-data';
import { ConfigBaseService } from './config-base.service';
import { PrimengOption } from '../entities/primeng-option';
import { AutoCompleteCompleteEvent } from 'primeng/autocomplete';

export interface AppConfig {
    inputStyle: string;
    colorScheme: string; 
    theme: string;
    ripple: boolean;
    menuMode: string;
    scale: number;
    color: string;
}

interface LayoutState {
    staticMenuDesktopInactive: boolean;
    overlayMenuActive: boolean;
    profileSidebarVisible: boolean;
    profileDropdownSidebarVisible:boolean;
    configSidebarVisible: boolean;
    staticMenuMobileActive: boolean;
    menuHoverActive: boolean;
}

@Injectable({
    providedIn: 'root',
})
export class LayoutBaseService implements OnDestroy {
    userSubscription: Subscription;

    protected _unreadNotificationsNumber = new BehaviorSubject<number | null>(null);
    unreadNotificationsCount$ = this._unreadNotificationsNumber.asObservable();

    layoutConfig: AppConfig = {
        ripple: false,
        inputStyle: 'outlined',
        menuMode: 'static',
        colorScheme: 'light',
        theme: 'lara-light-indigo',
        scale: 14,
        color: `var(--primary-color)`,
    };

    state: LayoutState = {
        staticMenuDesktopInactive: false,
        overlayMenuActive: false,
        profileSidebarVisible: false,
        profileDropdownSidebarVisible: false,
        configSidebarVisible: false,
        staticMenuMobileActive: false,
        menuHoverActive: false
    };

    private configUpdate = new Subject<AppConfig>();

    private overlayOpen = new Subject<any>();

    configUpdate$ = this.configUpdate.asObservable();

    overlayOpen$ = this.overlayOpen.asObservable();

    constructor(
        protected apiService: ApiSecurityService,
        protected config: ConfigBaseService,
        protected authService: AuthBaseService
    ) {
    }

    initUnreadNotificationsCountForCurrentUser = () => {
        this.initUnreadNotificationsCountForCurrentUserObservable().pipe(
            delay(1), // FT HACK: Adding delay so both additionalObservable and user emits single null value when logout
            withLatestFrom(this.authService.user$) // Triggers when unread notifications change
        ).subscribe(([additionalObservable, user]) => {
            if (user != null && additionalObservable !== undefined) {
                this.setUnreadNotificationsCountForCurrentUser().subscribe();
            }
        });
    }

    initUnreadNotificationsCountForCurrentUserObservable = (): Observable<any> => {
        return this.authService.user$;
    }

    setUnreadNotificationsCountForCurrentUser = (): Observable<any> => {
        return this.apiService.getUnreadNotificationsCountForCurrentUser().pipe(
            map((unreadNotificationsCount) => {
                this._unreadNotificationsNumber.next(unreadNotificationsCount);
            })
        );
    }

    onMenuToggle() {
        if (this.isOverlay()) {
            this.state.overlayMenuActive = !this.state.overlayMenuActive;
            if (this.state.overlayMenuActive) {
                this.overlayOpen.next(null);
            }
        }

        if (this.isDesktop()) {
            this.state.staticMenuDesktopInactive = !this.state.staticMenuDesktopInactive;
        }
        else {
            this.state.staticMenuMobileActive = !this.state.staticMenuMobileActive;

            if (this.state.staticMenuMobileActive) {
                this.overlayOpen.next(null);
            }
        }
    }

    showProfileSidebar() {
        this.state.profileSidebarVisible = !this.state.profileSidebarVisible;
        if (this.state.profileSidebarVisible) {
            this.overlayOpen.next(null);
        }
    }

    showProfileDropdownSidebar() {
        this.state.profileDropdownSidebarVisible = !this.state.profileDropdownSidebarVisible;
        if (this.state.profileDropdownSidebarVisible) {
            this.overlayOpen.next(null);
        }
    }

    showConfigSidebar() {
        this.state.configSidebarVisible = true;
    }

    isOverlay() {
        return this.layoutConfig.menuMode === 'overlay';
    }

    isDesktop() {
        return window.innerWidth > 991;
    }

    isMobile() {
        return !this.isDesktop();
    }

    onConfigUpdate() {
        this.configUpdate.next(this.layoutConfig);
    }
    
    //#region Top Bar

    initTopBarData = (): Observable<InitTopBarData> => {
        return combineLatest([this.authService.user$, this.unreadNotificationsCount$]).pipe(
            map(([currentUser, unreadNotificationsCount]) => {
                return new InitTopBarData({
                    companyName: this.config.companyName,
                    userProfilePath: `/administration/users/${currentUser?.id}`,
                    unreadNotificationsCount: unreadNotificationsCount,
                    showProfileIcon: true,
                    currentUser: currentUser,
                });
            })
        );
    }

    //#endregion

    //#region Side Bar

    searchPartners = (event: AutoCompleteCompleteEvent): Observable<PrimengOption[]> => {
        return of();
    }

    partnersAutocompleteButtonClick = (selectedPartner: PrimengOption) => {}

    //#endregion

    ngOnDestroy(): void {
        if (this.userSubscription) {
            this.userSubscription.unsubscribe();
        }
    }
}
