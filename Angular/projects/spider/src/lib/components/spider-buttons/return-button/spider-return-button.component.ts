import { Component, Input } from "@angular/core";
import { Router } from '@angular/router';
import { CommonModule } from "@angular/common";
import { getParentUrl } from "../../../services/helper-functions";
import { TranslocoDirective } from "@jsverse/transloco";
import { ButtonModule } from "primeng/button";
import { SpiderButtonComponent } from "../spider-button/spider-button.component";

@Component({
  selector: 'spider-return-button',
  templateUrl: './spider-return-button.component.html',
  styles: [],
  imports: [
    CommonModule,
    ButtonModule,
    SpiderButtonComponent,
    TranslocoDirective,
  ],
  standalone: true,
})
export class SpiderReturnButtonComponent {
  @Input() navigateUrl: string;

  constructor(private router: Router) {}

  onReturn(){
    if(this.navigateUrl == undefined){
        const currentUrl = this.router.url;
        const parentUrl: string = getParentUrl(currentUrl);
        this.router.navigateByUrl(parentUrl);
    }
    else{
        this.router.navigate([this.navigateUrl]);
    }
  }
}