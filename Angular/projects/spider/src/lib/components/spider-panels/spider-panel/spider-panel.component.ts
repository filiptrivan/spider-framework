import { Component, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { Menu } from 'primeng/menu';

@Component({
  selector: 'spider-panel',
  templateUrl: './spider-panel.component.html',
})
export class SpiderPanelComponent implements OnInit {
  @Input() isFirstMultiplePanel: boolean = false;
  @Input() isMiddleMultiplePanel: boolean = false;
  @Input() isLastMultiplePanel: boolean = false;
  @Input() crudMenu: MenuItem[];
  @Input() showCrudMenu: boolean = true;
  @Input() showRemoveIcon: boolean = false;
  @Input() index: number;
  @Input() showPanelHeader: boolean = true;

  @Output() onMenuIconClick: EventEmitter<number> = new EventEmitter();
  @Output() onRemoveIconClick: EventEmitter<null> = new EventEmitter();

  @ViewChild('menu') menu: Menu;
  
  constructor() { }

  ngOnInit(): void {
  }

  menuItemClick(index: number, event){
    this.menu.toggle(event);
    this.onMenuIconClick.next(index);
  }

  removeItemClick(){
    this.onRemoveIconClick.next(null);
  }

}