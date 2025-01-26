import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'spider-card',
  templateUrl: './spider-card.component.html',
  styles: []
})
export class SpiderCardComponent implements OnInit {
  @Input() icon: string = 'pi pi-file-edit';
  @Input() title: string;

  constructor() { }

  ngOnInit(): void {
  }
}