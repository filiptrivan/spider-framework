import { TranslocoService } from '@jsverse/transloco';
import { NavigationEnd, Router } from '@angular/router';
import { Component, ElementRef, OnDestroy, ViewChild } from '@angular/core';
import { filter, Subscription } from 'rxjs';
import { AuthBaseService } from '../../../services/auth-base.service';
import { LayoutBaseService } from '../../../services/app-layout-base.service';
import { ApiSecurityService } from '../../../services/api.service.security';
import { ConfigBaseService } from '../../../services/config-base.service';
import { User } from '../../../entities/security-entities';
import { CommonModule } from '@angular/common';
import { PrimengModule } from '../../../modules/primeng.module';

interface SpiderMenuItem {
  label?: string;
  icon?: string;
  showSeparator?: boolean;
  onClick?: () => void;
  showNotificationBadge?: boolean;
}

@Component({
    selector: 'topbar',
    templateUrl: './topbar.component.html',
    styles: [
    ],
    standalone: true,
    imports: [
      CommonModule,
      PrimengModule
    ]
})
export class AppTopBarComponent implements OnDestroy {
  private initTopBarSubscription: Subscription | null = null;

  currentUser: User;
  userProfilePath: string;
  unreadNotificationsCount: number;
  menuItems: SpiderMenuItem[] = [];
  avatarLabel: string;
  companyName: string;
  showProfileIcon: boolean = false;

  @ViewChild('menubutton') menuButton!: ElementRef;

  @ViewChild('topbarmenu') menu!: ElementRef;

  @ViewChild('topbarprofiledropdownmenubutton') topbarProfileDropdownMenuButton!: ElementRef;

  constructor(
    public layoutService: LayoutBaseService, 
    private authService: AuthBaseService, 
    private apiService: ApiSecurityService,
    protected router: Router,
    private translocoService: TranslocoService,
    private config: ConfigBaseService
  ) { 
  }

  async ngOnInit(){
    this.menuItems = [
      {
        label: this.translocoService.translate('YourProfile'),
        icon: 'pi-user',
        showSeparator: true,
        onClick: () => {
          this.routeToUserPage();
        }
      },
      {
        label: this.translocoService.translate('NotificationList'),
        icon: 'pi-bell',
        showNotificationBadge: true,
        onClick: () => {
          this.router.navigateByUrl(`/notifications`);
        },
      },
      // {
      //   label: this.translocoService.translate('Settings'),
      //   icon: 'pi-cog'
      // },
      {
        label: this.translocoService.translate('Logout'),
        icon: 'pi-sign-out',
        showSeparator: true,
        onClick: () => {
          this.authService.logout();
        }
      }
    ];

    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: NavigationEnd) => {
        this.layoutService.state.profileDropdownSidebarVisible = false;
      });

    this.onAfterNgOnInit();
  }

  onAfterNgOnInit = () => {
    this.initTopBarSubscription = this.layoutService.initTopBarData().subscribe(initTopBarData => {
      this.userProfilePath = initTopBarData.userProfilePath;
      this.companyName = initTopBarData.companyName;
      this.unreadNotificationsCount = initTopBarData.unreadNotificationsCount;
      this.showProfileIcon = initTopBarData.showProfileIcon;
      this.currentUser = initTopBarData.currentUser;
      this.avatarLabel = initTopBarData.currentUser?.email.charAt(0).toLocaleUpperCase();
    });
  }

  onDocumentClick(event: any) {
    if (
      !this.menu.nativeElement.contains(event.target) 
    ) {
      if (this.layoutService.state.profileDropdownSidebarVisible == true) {
        this.layoutService.state.profileDropdownSidebarVisible = false;
      }
    }
  }

  routeToUserPage(){
    this.router.navigateByUrl(this.userProfilePath);
  }

  ngOnDestroy(): void {
    if (this.initTopBarSubscription) {
      this.initTopBarSubscription.unsubscribe();
    }
  }

}