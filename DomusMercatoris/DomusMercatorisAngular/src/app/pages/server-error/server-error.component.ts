import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'app-server-error',
    standalone: true,
    imports: [CommonModule, RouterLink],
    templateUrl: './server-error.component.html',
    styleUrl: './server-error.component.css'
})
export class ServerErrorComponent {
}
