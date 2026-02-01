import { Component, inject, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  authService = inject(AuthService);
  toastService = inject(ToastService);

  switchToLogin = output<void>();
  registrationSuccess = output<string>(); // emits email

  registerData = {
    firstName: '',
    lastName: '',
    email: '',
    password: ''
  };

  register() {
    const data = this.registerData;
    if (!data.firstName || !data.lastName || !data.email || !data.password) {
      this.toastService.error('Please fill in all fields.');
      return;
    }

    this.authService.register(data).subscribe({
      next: () => {
        this.toastService.success('Registration successful! You can now log in.');
        this.registrationSuccess.emit(data.email);
      },
      error: (err) => {
        this.toastService.error('Registration failed: ' + (err.error?.message || 'Unknown error'));
      }
    });
  }

  onSwitchToLogin() {
    this.switchToLogin.emit();
  }
}
