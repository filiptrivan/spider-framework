import { Component, EventEmitter, Input, Output } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ButtonModule } from "primeng/button";
import { SplitButtonModule } from "primeng/splitbutton";
import { Subject, Subscription, throttleTime } from "rxjs";
import { Router } from "@angular/router";
import { MenuItem } from "primeng/api";
import { SpiderButtonBaseComponent } from "../spider-button-base/spider-button-base";

@Component({
  selector: 'spider-button',
  templateUrl: './spider-button.component.html',
  styles: [],
  imports: [
    CommonModule,
    ButtonModule,
    SplitButtonModule
  ],
  standalone: true,
})
export class SpiderButtonComponent extends SpiderButtonBaseComponent {

  // constructor() {
  //   super();
    
  // }

}