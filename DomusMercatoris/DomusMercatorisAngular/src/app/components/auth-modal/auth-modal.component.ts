import { Component, inject, signal, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { LoginComponent } from '../login/login.component';
import { RegisterComponent } from '../register/register.component';

@Component({
  selector: 'app-auth-modal',
  standalone: true,
  imports: [CommonModule, LoginComponent, RegisterComponent],
  templateUrl: './auth-modal.component.html',
  styleUrl: './auth-modal.component.css'
})
export class AuthModalComponent {
  authService = inject(AuthService);

  // ViewChild to access child components if needed, or just rely on state
  @ViewChild(LoginComponent) loginComponent?: LoginComponent;

  close() {
    this.authService.closeLogin();
  }

  switchToRegister() {
    this.authService.isRegisterMode.set(true);
  }

  switchToLogin() {
    this.authService.isRegisterMode.set(false);
  }

  onRegistrationSuccess(email: string) {
    this.authService.isRegisterMode.set(false);
    // Optionally prefill email in login component if we could access it
    // Since we re-render login component, we might need a way to pass this data
    // But for now, user can just type it. Or we can use a signal in AuthService to share "last registered email"
    // Let's keep it simple for now.
    setTimeout(() => {
        if (this.loginComponent) {
            this.loginComponent.email = email;
        }
    });
  }
}
