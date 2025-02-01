import { Component, EventEmitter, Output } from "@angular/core";
import { TranslocoDirective } from "@jsverse/transloco";
import { ButtonModule } from "primeng/button";

@Component({
  selector: 'google-button',
  templateUrl: './google-button.component.html',
  styles: [],
  imports: [
    ButtonModule,
    TranslocoDirective,
  ],
  standalone: true,
})
export class GoogleButtonComponent { // Custom styling of the google button - https://medium.com/simform-engineering/implement-custom-google-sign-in-using-angular-16-9c93aeff6252
  // @Output() onClick: EventEmitter<any> = new EventEmitter();
  @Output() loginWithGoogle: EventEmitter<any> = new EventEmitter<any>();

  constructor() {}

  createFakeGoogleWrapper = () => {
    const googleLoginWrapper = document.createElement('div');
    googleLoginWrapper.style.display = 'none';
    googleLoginWrapper.classList.add('custom-google-button');
    document.body.appendChild(googleLoginWrapper);
    window.google.accounts.id.renderButton(googleLoginWrapper, {
      type: 'icon',
      width: '200',
    });
  
    const googleLoginWrapperButton = googleLoginWrapper.querySelector(
      'div[role=button]'
    ) as HTMLElement;
  
    return {
      click: () => {
        googleLoginWrapperButton?.click();
      },
    };
  };

  handleGoogleLogin() {
    this.loginWithGoogle.emit(this.createFakeGoogleWrapper());
  }

}

declare global {
  interface Window {
    google: any;
  }
}