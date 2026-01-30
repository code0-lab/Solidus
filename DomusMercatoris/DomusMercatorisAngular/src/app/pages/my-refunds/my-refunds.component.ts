import { Component, ChangeDetectionStrategy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RefundsService, RefundResponse } from '../../services/refunds.service';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  standalone: true,
  selector: 'app-my-refunds',
  imports: [CommonModule],
  templateUrl: './my-refunds.component.html',
  styleUrl: './my-refunds.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyRefundsComponent implements OnInit {
  refundsService = inject(RefundsService);
  router = inject(Router);
  authService = inject(AuthService);
  refunds = signal<RefundResponse[]>([]);

  ngOnInit(): void {
    if (!this.authService.currentUser()) {
      this.router.navigate(['/']);
      return;
    }
    this.loadRefunds();
  }

  loadRefunds() {
    this.refundsService.getMyRefunds().subscribe({
      next: (list) => this.refunds.set(list),
      error: () => { /* ignore for now */ }
    });
  }

  statusClass(status: RefundResponse['status']): string {
    switch (status) {
      case 'Pending': return 'badge bg-warning';
      case 'Approved': return 'badge bg-success';
      case 'Rejected': return 'badge bg-danger';
    }
  }
}
