import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { Company } from '../../models/user.model';

@Component({
  selector: 'app-auth-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './auth-modal.component.html',
  styleUrl: './auth-modal.component.css'
})
export class AuthModalComponent implements OnInit {
  authService = inject(AuthService);
  toastService = inject(ToastService);
  companies: Company[] = [];

  email = '';
  password = '';

  registerData = {
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    companyId: 0
  };

  ngOnInit() {
    this.authService.getCompanies().subscribe({
      next: (data) => {
        this.companies = data;
        if (this.companies.length > 0) {
          this.registerData.companyId = this.companies[0].companyId;
        }
      },
      error: (err) => console.error('Failed to load companies', err)
    });
  }

  close() {
    this.authService.closeLogin();
  }

  toggleMode() {
    this.authService.toggleAuthMode();
  }

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

  register() {
    const data = this.registerData;
    if (!data.firstName || !data.lastName || !data.email || !data.password) {
      this.toastService.error('Please fill in all fields.');
      return;
    }

    this.authService.register(data).subscribe({
      next: () => {
        this.toastService.success('Registration successful! You can now log in.');
        this.authService.isRegisterMode.set(false);
        this.email = data.email;
      },
      error: (err) => {
        this.toastService.error('Registration failed: ' + (err.error?.message || 'Unknown error'));
      }
    });
  }
}
