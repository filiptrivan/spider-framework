import { Component, EventEmitter, Input, Output } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ButtonModule } from "primeng/button";
import { SplitButtonModule } from "primeng/splitbutton";
import { Subject, Subscription, throttleTime } from "rxjs";
import { Router } from "@angular/router";
import { MenuItem } from "primeng/api";

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
export class SpiderButtonComponent {
  @Input() icon: string;
  @Input() label: string;
  @Input() outlined: boolean = false;
  @Input() rounded: boolean = false;
  @Input() styleClass: string;
  @Input() routerLink: string;
  @Input() style: { [klass: string]: any; };
  @Input() class: string;
  @Input() severity: 'success' | 'info' | 'warning' | 'danger' | 'help' | 'primary' | 'secondary' | 'contrast' | null | undefined;
  @Input() disabled: boolean = false;
  @Input() dropdownItems: MenuItem[];

  @Output() onClick = new EventEmitter<Event>();
  private clickSubject = new Subject<Event>(); // Internal subject to handle click events.
  private subscription: Subscription;

  constructor(
    private router: Router
  ) {
      
  }

  ngOnInit(){
    this.subscription = this.clickSubject
        .pipe(throttleTime(500))
        .subscribe((event: Event) => this.onClick.emit(event));
  }

  handleClick = (event: Event) => {
    event.preventDefault();
    event.stopPropagation();
    if (this.routerLink !== undefined) {
      this.router.navigate([this.routerLink]);
    }
    else{
      this.clickSubject.next(event);
    }
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }
}