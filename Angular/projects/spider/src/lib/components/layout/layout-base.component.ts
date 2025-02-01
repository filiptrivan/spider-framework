import { Component, OnDestroy, Renderer2, ViewChild } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { AppSidebarComponent } from './sidebar/sidebar.component';
import { AppTopBarComponent } from './topbar/topbar.component';
import { LayoutBaseService } from '../../services/app-layout-base.service';

@Component({
    selector: 'layout-base',
    template: '',
    standalone: true
})
export class LayoutBaseComponent implements OnDestroy {

    overlayMenuOpenSubscription: Subscription;

    menuOutsideClickListener: any;

    profileMenuOutsideClickListener: any;

    @ViewChild(AppSidebarComponent) appSidebar!: AppSidebarComponent;

    @ViewChild(AppTopBarComponent) appTopbar!: AppTopBarComponent;

    constructor(
        protected layoutService: LayoutBaseService, 
        protected renderer: Renderer2, 
        protected router: Router,
    ) {
        this.overlayMenuOpenSubscription = this.layoutService.overlayOpen$.subscribe(() => {
            if (!this.menuOutsideClickListener) {
                this.menuOutsideClickListener = this.renderer.listen('document', 'click', event => {
                    const isOutsideClicked = !(
                        this.appSidebar.el.nativeElement.isSameNode(event.target) || 
                        this.appSidebar.el.nativeElement.contains(event.target) ||
                        this.appTopbar.menuButton.nativeElement.isSameNode(event.target) || 
                        this.appTopbar.menuButton.nativeElement.contains(event.target) ||
                        (event.target.closest('.p-autocomplete-items')) ||
                        (event.target.closest('.p-autocomplete-clear-icon'))
                    );
                    
                    if (isOutsideClicked) {
                        this.hideMenu();
                    }
                });
            }

            if (!this.profileMenuOutsideClickListener) {
                this.profileMenuOutsideClickListener = this.renderer.listen('document', 'click', event => {
                    const isOutsideClicked = !(this.appTopbar.menu.nativeElement.isSameNode(event.target) || this.appTopbar.menu.nativeElement.contains(event.target));

                    if (isOutsideClicked) {
                        this.hideProfileMenu();
                    }
                });
            }

            if (this.layoutService.state.staticMenuMobileActive) {
                this.blockBodyScroll();
            }
        });

        this.router.events.pipe(filter(event => event instanceof NavigationEnd))
            .subscribe(() => {
                this.hideMenu();
                this.hideProfileMenu();
            });
    }

    hideMenu() {
        this.layoutService.state.overlayMenuActive = false;
        this.layoutService.state.staticMenuMobileActive = false;
        this.layoutService.state.menuHoverActive = false;
        if (this.menuOutsideClickListener) {
            this.menuOutsideClickListener();
            this.menuOutsideClickListener = null;
        }
        this.unblockBodyScroll();
    }

    hideProfileMenu() {
        this.layoutService.state.profileSidebarVisible = false;
        if (this.profileMenuOutsideClickListener) {
            this.profileMenuOutsideClickListener();
            this.profileMenuOutsideClickListener = null;
        }
    }

    blockBodyScroll(): void {
        if (document.body.classList) {
            document.body.classList.add('blocked-scroll');
        }
        else {
            document.body.className += ' blocked-scroll';
        }
    }

    unblockBodyScroll(): void {
        if (document.body.classList) {
            document.body.classList.remove('blocked-scroll');
        }
        else {
            document.body.className = document.body.className.replace(new RegExp('(^|\\b)' +
                'blocked-scroll'.split(' ').join('|') + '(\\b|$)', 'gi'), ' ');
        }
    }

    get containerClass() {
        return {
            'layout-theme-light': this.layoutService.layoutConfig.colorScheme === 'light',
            'layout-theme-dark': this.layoutService.layoutConfig.colorScheme === 'dark',
            'layout-overlay': this.layoutService.layoutConfig.menuMode === 'overlay',
            'layout-static': this.layoutService.layoutConfig.menuMode === 'static',
            'layout-static-inactive': this.layoutService.state.staticMenuDesktopInactive && this.layoutService.layoutConfig.menuMode === 'static',
            'layout-overlay-active': this.layoutService.state.overlayMenuActive,
            'layout-mobile-active': this.layoutService.state.staticMenuMobileActive,
            'p-input-filled': this.layoutService.layoutConfig.inputStyle === 'filled',
            'p-ripple-disabled': !this.layoutService.layoutConfig.ripple
        }
    }

    ngOnDestroy() {
        if (this.overlayMenuOpenSubscription) {
            this.overlayMenuOpenSubscription.unsubscribe();
        }

        if (this.menuOutsideClickListener) {
            this.menuOutsideClickListener();
        }

        this.onAfterNgDestroy();
    }

    onAfterNgDestroy = () => {}
    
}

