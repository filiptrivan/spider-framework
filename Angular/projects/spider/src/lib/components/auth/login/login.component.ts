import { ActivatedRoute, Router } from '@angular/router';
import { ChangeDetectorRef, Component, KeyValueDiffers, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { TranslocoService } from '@jsverse/transloco';
import { BaseFormCopy } from '../../base-form/base-form copy';
import { Login } from '../../../entities/security-entities';
import { SpiderFormGroup } from '../../spider-form-control/spider-form-control';
import { SpiderMessageService } from '../../../services/spider-message.service';
import { BaseFormService } from '../../../services/base-form.service';
import { AuthBaseService } from '../../../services/auth-base.service';
import { ConfigBaseService } from '../../../services/config-base.service';

@Component({
    selector: 'app-login',
    templateUrl: './login.component.html',
})
export class LoginComponent extends BaseFormCopy implements OnInit {
    loginFormGroup = new SpiderFormGroup<Login>({});

    companyName: string;
    showEmailSentDialog: boolean = false;
    usersCanRegister: boolean = this.config.usersCanRegister;

    constructor(
      protected override differs: KeyValueDiffers,
      protected override http: HttpClient,
      protected override messageService: SpiderMessageService, 
      protected override changeDetectorRef: ChangeDetectorRef,
      protected override router: Router, 
      protected override route: ActivatedRoute,
      protected override translocoService: TranslocoService,
      protected override baseFormService: BaseFormService,
      private authService: AuthBaseService, 
      private config: ConfigBaseService
    ) { 
      super(differs, http, messageService, changeDetectorRef, router, route, translocoService, baseFormService);
    }

    override ngOnInit(){
        this.initLoginFormGroup(new Login({}));
    }
    
    initLoginFormGroup(model: Login){
      this.initFormGroup(this.loginFormGroup, this.formGroup, model, model.typeName, []);
    }

    companyNameChange(companyName: string){
      this.companyName = companyName;
    }

    sendLoginVerificationEmail() {
        let isFormGroupValid: boolean = this.baseFormService.checkFormGroupValidity(this.loginFormGroup);
        
        if (isFormGroupValid == false) 
          return;

        this.authService.sendLoginVerificationEmail(this.loginFormGroup.getRawValue()).subscribe(()=>{
            this.showEmailSentDialog = true;
        });
    }

}
