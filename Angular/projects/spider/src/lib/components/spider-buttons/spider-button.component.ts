import { Component, EventEmitter, HostListener, Input, Output } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ButtonModule } from "primeng/button";
import { Subject, Subscription, throttleTime } from "rxjs";

@Component({
  selector: 'spider-button',
  templateUrl: './spider-button.component.html',
  styles: [],
  imports: [
    CommonModule,
    ButtonModule,
  ],
  standalone: true,
})
export class SpiderButtonComponent {
    @Input() icon: string;
    @Input() label: string;
    @Input() outlined: boolean = false;
    @Input() rounded: boolean = false;
    @Input() styleClass: string;
    @Input() severity: 'success' | 'info' | 'warning' | 'danger' | 'help' | 'primary' | 'secondary' | 'contrast' | null | undefined;

    @Output() onClick = new EventEmitter<Event>();
    private clickSubject = new Subject<Event>(); // Internal subject to handle click events.
    private subscription: Subscription;

    constructor() {
        
    }

    ngOnInit(){
        this.subscription = this.clickSubject
            .pipe(throttleTime(500))
            .subscribe((event: Event) => this.onClick.emit(event));
    }

    handleClick = (event: Event) => {
        event.preventDefault();
        event.stopPropagation();
        this.clickSubject.next(event);
    }

    ngOnDestroy() {
        this.subscription.unsubscribe();
    }
}