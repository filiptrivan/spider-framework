import { Component, Input } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ButtonModule } from "primeng/button";
import { SplitButtonModule } from "primeng/splitbutton";

import { MenuItem } from "primeng/api";
import { SpiderButtonBaseComponent } from "../spider-button-base/spider-button-base";

@Component({
  selector: 'spider-split-button',
  templateUrl: './spider-split-button.component.html',
  styles: [],
  imports: [
    CommonModule,
    ButtonModule,
    SplitButtonModule
  ],
  standalone: true,
})
export class SpiderSplitButtonComponent extends SpiderButtonBaseComponent {
  @Input() dropdownItems: MenuItem[];



}