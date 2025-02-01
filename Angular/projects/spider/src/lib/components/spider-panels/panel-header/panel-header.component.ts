import { TranslocoService } from '@jsverse/transloco';
import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'panel-header',
  templateUrl: './panel-header.component.html',
  styles: [`
    .p-panel-icons-end {
      font-size: 50px;
    }
  `]
})
export class PanelHeaderComponent implements OnInit {
  @Input() icon: string = 'pi pi-file-edit';
  @Input() title: string;
  @Input() bigTitle: boolean;
  @Input() index: number;
  @Input() tabs: SpiderTab[];

  constructor(
    private translocoService: TranslocoService
  ) { }

  ngOnInit(): void {
    if (this.title == null)
      this.title = this.translocoService.translate('Details')
  }

  setTabIsSelected(tab: SpiderTab){
    this.tabs.forEach(t => {
      t.isSelected = false;
    });

    tab.isSelected = true;
  }
}

export class SpiderTab
{
    label?: string;
    value?: number;
    icon?: string;
    isSelected?: boolean;
  
    constructor(
    {
        label,
        value,
        icon,
        isSelected,
    }:{
        label?: string;
        value?: number;
        icon?: string;
        isSelected?: boolean;
    } = {}
    ) {
        this.label = label;
        this.value = value;
        this.icon = icon;
        this.isSelected = isSelected;
    }

}