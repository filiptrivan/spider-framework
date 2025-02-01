import { ChangeDetectorRef, Component, HostBinding, Input, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import { SidebarMenuService } from './sidebar-menu.service';
import { SpiderMenuItem } from './sidebar-menu.component';
import { AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { SpiderFormControl } from '../../spider-form-control/spider-form-control';
import { PrimengOption } from '../../../entities/primeng-option';
import { LayoutBaseService } from '../../../services/app-layout-base.service';
import { AuthBaseService } from '../../../services/auth-base.service';
import { ApiSecurityService } from '../../../services/api.service.security';
import { ConfigBaseService } from '../../../services/config-base.service';
import { CommonModule } from '@angular/common';
import { PrimengModule } from '../../../modules/primeng.module';
import { SpiderControlsModule } from '../../../controls/spider-controls.module';
import { TranslocoDirective } from '@jsverse/transloco';

@Component({
    // eslint-disable-next-line @angular-eslint/component-selector
    selector: '[menuitem]',
    templateUrl: './menuitem.component.html',
    animations: [
        trigger('children', [
            state('collapsed', style({
                height: '0'
            })),
            state('expanded', style({
                height: '*'
            })),
            transition('collapsed <=> expanded', animate('400ms cubic-bezier(0.86, 0, 0.07, 1)'))
        ])
    ],
    standalone: true,
    imports: [
        CommonModule,
        PrimengModule,
        TranslocoDirective,
        SpiderControlsModule,
    ]
})
export class MenuitemComponent implements OnInit, OnDestroy {

    @Input() item: SpiderMenuItem;

    @Input() index!: number;

    @Input() @HostBinding('class.layout-root-menuitem') root!: boolean;

    @Input() parentKey!: string;

    active = false;

    private menuSourceSubscription: Subscription;

    private menuResetSubscription: Subscription;

    private permissionSubscription: Subscription | null = null;

    key: string = "";

    selectedPartner: SpiderFormControl = new SpiderFormControl<string>(null, { updateOn: 'change' });

    partnerOptions: PrimengOption[];

    constructor(
        public layoutService: LayoutBaseService, 
        private cd: ChangeDetectorRef, 
        public router: Router, 
        private menuService: SidebarMenuService, 
        private authService: AuthBaseService,
        private apiService: ApiSecurityService,
        private config: ConfigBaseService
    ) {
        this.menuSourceSubscription = this.menuService.menuSource$.subscribe(value => {
            Promise.resolve(null).then(() => {
                if (value.routeEvent) {
                    this.active = (value.key === this.key || value.key.startsWith(this.key + '-')) ? true : false;
                }
                else {
                    if (value.key !== this.key && !value.key.startsWith(this.key + '-')) {
                        this.active = false;
                    }
                }
            });
        });

        this.menuResetSubscription = this.menuService.resetSource$.subscribe(() => {
            this.active = false;
        });

        this.router.events.pipe(filter(event => event instanceof NavigationEnd))
            .subscribe(params => {
                if (this.item.routerLink) {
                    this.updateActiveStateFromRoute();
                }
            });
    }

    ngOnInit() {
        this.key = this.parentKey ? this.parentKey + '-' + this.index : String(this.index);
        
        if (this.item.routerLink) {
            this.updateActiveStateFromRoute();
        }

        this.permissionSubscription = this.authService.currentUserPermissions$.subscribe((currentUserPermissionCodes: string[]) => {
            if (this.item && typeof this.item.hasPermission === 'function') {
                this.item.visible = this.item.hasPermission(currentUserPermissionCodes);
            }
        });
    }

    updateActiveStateFromRoute() {
        let activeRoute = this.router.isActive(this.item.routerLink[0], { paths: 'exact', queryParams: 'ignored', matrixParams: 'ignored', fragment: 'ignored' });

        if (activeRoute) {
            this.menuService.onMenuStateChange({ key: this.key, routeEvent: true });
        }
    }

    itemClick(event: Event) {
        // avoid processing disabled items
        if (this.item.disabled || event === null) {
            event.preventDefault();
            return;
        }

        // execute command
        if (this.item.command) {
            this.item.command({ originalEvent: event, item: this.item });
        }

        // toggle active state
        if (this.item.items) {
            this.active = !this.active;
        }
        
        this.menuService.onMenuStateChange({ key: this.key });
    }

    get submenuAnimation() {
        return this.root ? 'expanded' : (this.active ? 'expanded' : 'collapsed');
    }

    @HostBinding('class.active-menuitem') 
    get activeClass() {
        return this.active && !this.root;
    }

    //#region FT HACK: Partner

    searchPartners(event: AutoCompleteCompleteEvent) {
        this.layoutService.searchPartners(event).subscribe(po => {
            this.partnerOptions = po;    
        })
    }

    partnersAutocompleteButtonClick = async () => {
        this.layoutService.partnersAutocompleteButtonClick(this.selectedPartner);
    }

    //#endregion

    ngOnDestroy() {
        if (this.menuSourceSubscription) {
            this.menuSourceSubscription.unsubscribe();
        }

        if (this.menuResetSubscription) {
            this.menuResetSubscription.unsubscribe();
        }

        if (this.permissionSubscription) {
            this.permissionSubscription.unsubscribe();
        }
    }
}
