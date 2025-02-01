import { Component, EventEmitter, Input, Output } from "@angular/core";
import { CommonModule } from "@angular/common";
import { Subscription } from "rxjs";
import { TranslocoDirective } from "@jsverse/transloco";
import { GoogleButtonComponent } from "../../google-button/google-button.component";
import { ConfigBaseService } from "../../../services/config-base.service";
import { AuthBaseService } from "../../../services/auth-base.service";

@Component({
  selector: 'auth',
  templateUrl: './auth.component.html',
  styles: [],
  imports: [
    CommonModule,
    GoogleButtonComponent,
    TranslocoDirective,
  ],
  standalone: true,
})
export class AuthComponent {
  private initCompanyAuthDialogDetailsSubscription: Subscription | null = null;

  @Output() onCompanyNameChange: EventEmitter<string> = new EventEmitter();
  @Input() showGoogleAuth: boolean = true;

  hasGoogleAuth: boolean = this.config.googleAuth;
  companyName: string;
  image: string;
  
  constructor(
    private config: ConfigBaseService,
    private authService: AuthBaseService,
  ) {

  }

  ngOnInit(){
    this.initCompanyDetails();
  }

  initCompanyDetails() {
    this.initCompanyAuthDialogDetailsSubscription = this.authService.initCompanyAuthDialogDetails().subscribe(initCompanyAuthDialogDetails => {
      this.image = initCompanyAuthDialogDetails.image;
      this.companyName = initCompanyAuthDialogDetails.companyName;
      this.onCompanyNameChange.next(this.companyName);
    })
  }

  onGoogleSignIn(googleWrapper: any){
    googleWrapper.click();
  }

  ngOnDestroy(): void {
    if (this.initCompanyAuthDialogDetailsSubscription) {
      this.initCompanyAuthDialogDetailsSubscription.unsubscribe();
    }
  }
}

