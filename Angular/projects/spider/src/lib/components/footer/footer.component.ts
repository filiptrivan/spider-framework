import { Component } from '@angular/core';
import { ConfigBaseService } from '../../services/config-base.service'

@Component({
    selector: 'footer',
    templateUrl: './footer.component.html',
    standalone: true
})
export class FooterComponent {
    companyName: string = this.config.companyName;

    constructor(
        private config: ConfigBaseService
    ) { 

    }
}
