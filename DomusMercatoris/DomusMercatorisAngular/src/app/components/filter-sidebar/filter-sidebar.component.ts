import { Component, ChangeDetectionStrategy, model, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-filter-sidebar',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './filter-sidebar.component.html',
    styleUrl: './filter-sidebar.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class FilterSidebarComponent {
    // Using model signals for two-way binding (requires Angular 17.2+)
    // If older Angular, fallback to @Input + @Output. Assuming modern based on signals usage.
    minPrice = model<number | null>(null);
    maxPrice = model<number | null>(null);

    // Open state passed from parent
    isOpen = input<boolean>(false);
}
