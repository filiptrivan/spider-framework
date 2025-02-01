import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { RegistrationComponent } from './registration/registration.component';
import { LoginComponent } from './login/login.component';
import { AuthComponent } from './partials/auth.component';
import { TranslocoDirective } from '@jsverse/transloco';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { PrimengModule } from '../../modules/primeng.module';
import { SpiderControlsModule } from '../../controls/spider-controls.module';
import { LoginVerificationComponent } from './partials/login-verification.component';
import { RegistrationVerificationComponent } from './partials/registration-verification.component';

const routes: Routes = [
    { 
        path: 'registration', 
        component: RegistrationComponent
    },
    { 
        path: 'login', 
        component: LoginComponent
    },
];

@NgModule({
    imports: [
        RouterModule.forChild(routes),
        CommonModule,
        ReactiveFormsModule,
        AuthComponent,
        PrimengModule,
        SpiderControlsModule,
        LoginVerificationComponent,
        RegistrationVerificationComponent,
        TranslocoDirective,
    ],
    declarations: [
        RegistrationComponent,
        LoginComponent,
    ]
})
export class AuthModule { }
