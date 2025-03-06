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
  @Input() title: string;
  @Input() showBigTitle: boolean;
  @Input() showIcon: boolean = true;
  @Input() icon: string;
  @Input() index: number;
  @Input() tabs: SpiderTab[];

  constructor(
    private translocoService: TranslocoService
  ) { }

  ngOnInit(): void {
    if (this.title == null)
      this.title = this.translocoService.translate('Details');

    if (this.showIcon === true && this.icon == null)
      this.icon = 'pi pi-file-edit';
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
    id?: number;
    icon?: string;
    isSelected?: boolean;
  
    constructor(
    {
        label,
        id,
        icon,
        isSelected,
    }:{
        label?: string;
        id?: number;
        icon?: string;
        isSelected?: boolean;
    } = {}
    ) {
        this.label = label;
        this.id = id;
        this.icon = icon;
        this.isSelected = isSelected;
    }

}