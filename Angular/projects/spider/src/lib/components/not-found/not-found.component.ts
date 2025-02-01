import { Component } from '@angular/core';
import { ConfigBaseService } from '../../services/config-base.service';
import { RouterModule } from '@angular/router';

@Component({
    selector: 'not-found',
    templateUrl: './not-found.component.html',
    standalone: true,
    imports: [
        RouterModule
    ]
})
export class NotFoundComponent { 
    companyName = this.config.companyName;

    constructor(
        private config: ConfigBaseService
    ) { 

    }
}