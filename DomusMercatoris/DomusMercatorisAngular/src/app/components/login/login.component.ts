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

    // Regex check for login (MVC compatible: at least one lowercase, one uppercase, one number, min 5 chars)
    // Note: Angular Register enforces stricter rules (special char, min 8), but we use looser rules here to allow MVC users to login.
    const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{5,}$/;
    if (!passwordRegex.test(this.password)) {
       this.toastService.error('Password format is invalid (must contain uppercase, lowercase, number and be 5+ chars).');
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
