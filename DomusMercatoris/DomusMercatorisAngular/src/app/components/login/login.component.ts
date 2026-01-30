import { Component, inject, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  authService = inject(AuthService);
  toastService = inject(ToastService);
  
  switchToRegister = output<void>();

  email = '';
  password = '';

  login() {
    if (!this.email || !this.password) {
      this.toastService.error('Please enter email and password.');
      return;
    }

    this.authService.login({ email: this.email, password: this.password }).subscribe({
      next: () => {
        this.toastService.success('Login successful!');
        this.email = '';
        this.password = '';
      },
      error: (err) => {
        this.toastService.error('Login failed: ' + (err.error?.message || 'Unknown error'));
      }
    });
  }

  onSwitchToRegister() {
    this.switchToRegister.emit();
  }
}
