import { Component, inject, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { ValidationConstants } from '../../constants/validation.constants';

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
  
  showTestMenu = false;

  toggleTestMenu() {
    this.showTestMenu = !this.showTestMenu;
  }

  fillCredentials(email: string, pass: string) {
    this.email = email;
    this.password = pass;
    this.login();
  }

  login() {
    if (!this.email || !this.password) {
      this.toastService.error('Please enter email and password.');
      return;
    }

    // Regex check for login (using centralized validation constants)
    const passwordRegex = ValidationConstants.password.regex;
    if (!passwordRegex.test(this.password)) {
       this.toastService.error(ValidationConstants.password.errorMessage);
       return;
    }

    this.authService.login({ email: this.email, password: this.password }).subscribe(() => {
      this.toastService.success('Login successful!');
      this.email = '';
      this.password = '';
    });
  }

  onSwitchToRegister() {
    this.switchToRegister.emit();
  }
}
