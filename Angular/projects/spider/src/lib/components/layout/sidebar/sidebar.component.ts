import { Component, ElementRef, Input } from '@angular/core';
import { SidebarMenuComponent, SpiderMenuItem } from './sidebar-menu.component';

@Component({
    selector: 'sidebar',
    templateUrl: './sidebar.component.html',
    standalone: true,
    imports: [
        SidebarMenuComponent
    ]
})
export class AppSidebarComponent {
    @Input() menu: SpiderMenuItem[];

    constructor(public el: ElementRef) { 
    }

    ngOnInit(){
    }
}

