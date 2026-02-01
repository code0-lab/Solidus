import { Component, inject, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { ValidationConstants } from '../../constants/validation.constants';

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

    const passwordRegex = ValidationConstants.password.regex;
    if (!passwordRegex.test(data.password)) {
      this.toastService.error(ValidationConstants.password.errorMessage);
      return;
    }

    this.authService.register(data).subscribe(() => {
      this.toastService.success('Registration successful! You can now log in.');
      this.registrationSuccess.emit(data.email);
    });
  }

  generateRandomPassword() {
    const length = 12;
    const lowers = "abcdefghijklmnopqrstuvwxyz";
    const uppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const digits = "0123456789";
    const specials = "@$!%*?&";
    const all = lowers + uppers + digits + specials;

    let password = "";
    // Ensure one of each required type (keeping this for stronger random passwords even if validation is relaxed)
    password += lowers.charAt(Math.floor(Math.random() * lowers.length));
    password += uppers.charAt(Math.floor(Math.random() * uppers.length));
    password += digits.charAt(Math.floor(Math.random() * digits.length));
    password += specials.charAt(Math.floor(Math.random() * specials.length));

    // Fill rest
    for (let i = 4; i < length; i++) {
        password += all.charAt(Math.floor(Math.random() * all.length));
    }

    // Shuffle
    password = password.split('').sort(() => 0.5 - Math.random()).join('');

    this.registerData.password = password;
    
    // Show password to user
    const input = document.getElementById('registerPassword') as HTMLInputElement;
    if(input) {
      input.type = 'text';
      setTimeout(() => {
         // Optional: hide after some time or let user hide it
      }, 5000);
    }
    this.toastService.success('Random password generated: ' + password);
  }

  onSwitchToLogin() {
    this.switchToLogin.emit();
  }
}
