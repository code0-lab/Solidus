import { Component, inject, OnInit, ChangeDetectionStrategy, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProfileComponent implements OnInit {
  authService = inject(AuthService);
  router = inject(Router);
  destroyRef = inject(DestroyRef);
  fb = inject(FormBuilder);
  toastService = inject(ToastService);

  profileForm!: FormGroup;
  passwordForm!: FormGroup;
  emailForm!: FormGroup;

  isSaving = signal(false);
  showLogoutConfirm = signal(false);
  activeTab = signal<'overview' | 'contact' | 'password' | 'email'>('overview');

  ngOnInit() {
    const user = this.authService.currentUser();
    if (!user) {
      this.router.navigate(['/']);
      return;
    }
    
    this.profileForm = this.fb.group({
      phone: [user?.phone ?? '', [Validators.pattern('^[0-9+ ]*$')]],
      address: [user?.address ?? '', [Validators.maxLength(500)]]
    });

    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmNewPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });

    this.emailForm = this.fb.group({
      newEmail: ['', [Validators.required, Validators.email]],
      currentPassword: ['', Validators.required]
    });
  }

  passwordMatchValidator(g: FormGroup) {
    return g.get('newPassword')?.value === g.get('confirmNewPassword')?.value
      ? null : { mismatch: true };
  }

  setActiveTab(tab: 'overview' | 'contact' | 'password' | 'email') {
    this.activeTab.set(tab);
    if (tab === 'password') this.passwordForm.reset();
    if (tab === 'email') this.emailForm.reset();
  }

  // Helper to check if form value is different from initial user data
  get hasChanges(): boolean {
    const user = this.authService.currentUser();
    if (!user) return false;
    
    const currentVal = this.profileForm.value;
    const initialPhone = user.phone ?? '';
    const initialAddress = user.address ?? '';

    // Handle null/undefined vs empty string equality
    const phoneChanged = (currentVal.phone?.trim() ?? '') !== initialPhone;
    const addressChanged = (currentVal.address?.trim() ?? '') !== initialAddress;

    return phoneChanged || addressChanged;
  }

  saveProfile() {
    if (this.profileForm.invalid || !this.hasChanges) {
      return;
    }

    this.isSaving.set(true);
    const formValue = this.profileForm.value;
    
    const payload = {
      phone: formValue.phone?.trim() || null,
      address: formValue.address?.trim() || null
    };

    this.authService.updateProfile(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: profile => {
          this.profileForm.reset({
            phone: profile.phone ?? '',
            address: profile.address ?? ''
          });
          this.isSaving.set(false);
          this.toastService.success('Profile updated successfully.');
        },
        error: () => {
          this.isSaving.set(false);
          this.toastService.error('Failed to update profile.');
        }
      });
  }

  changePassword() {
    if (this.passwordForm.invalid) return;

    this.isSaving.set(true);
    const { currentPassword, newPassword, confirmNewPassword } = this.passwordForm.value;

    this.authService.changePassword({ currentPassword, newPassword, confirmNewPassword })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.passwordForm.reset();
          this.toastService.success('Password changed successfully.');
          this.setActiveTab('overview');
        },
        error: (err) => {
          this.isSaving.set(false);
          this.toastService.error(err.error?.message || 'Failed to change password.');
        }
      });
  }

  changeEmail() {
    if (this.emailForm.invalid) return;

    this.isSaving.set(true);
    const { newEmail, currentPassword } = this.emailForm.value;

    this.authService.changeEmail({ newEmail, currentPassword })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.emailForm.reset();
          this.toastService.success('Email changed successfully.');
          this.setActiveTab('overview');
        },
        error: (err) => {
          this.isSaving.set(false);
          this.toastService.error(err.error?.message || 'Failed to change email.');
        }
      });
  }

  logout() {
    this.showLogoutConfirm.set(true);
  }

  confirmLogout() {
    this.authService.logout();
    this.showLogoutConfirm.set(false);
    this.router.navigate(['/']);
    this.toastService.info('Logged out successfully.');
  }

  cancelLogout() {
    this.showLogoutConfirm.set(false);
  }
}
