import { Component, Input } from "@angular/core";
import { SkeletonModule } from 'primeng/skeleton';

@Component({
  selector: 'card-skeleton',
  templateUrl: './card-skeleton.component.html',
  styles: [],
  standalone: true,
  imports: [SkeletonModule]
})
export class CardSkeletonComponent {
  @Input() height: number = 400;
  titleHeight: number = 23;
  dataHeight: number;
  padding: number = 21;
  titleMarginBottom: number = 14;
  titleMarginTop: number = 4;

  ngOnInit(){
    this.dataHeight = this.height - (this.titleHeight + this.padding * 2 + this.titleMarginBottom + this.titleMarginTop)
  }
}