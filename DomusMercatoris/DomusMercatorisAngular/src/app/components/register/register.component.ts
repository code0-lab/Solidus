import { Component, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { Company } from '../../models/user.model';

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

  companies = input<Company[]>([]);
  switchToLogin = output<void>();
  registrationSuccess = output<string>(); // emits email

  registerData = {
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    companyId: 0
  };

  ngOnInit() {
    // Default to first company if available
    const comps = this.companies();
    if (comps.length > 0 && this.registerData.companyId === 0) {
      this.registerData.companyId = comps[0].companyId;
    }
  }

  // Update companyId when companies input changes if needed
  ngOnChanges() {
    const comps = this.companies();
    if (comps.length > 0 && this.registerData.companyId === 0) {
      this.registerData.companyId = comps[0].companyId;
    }
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
