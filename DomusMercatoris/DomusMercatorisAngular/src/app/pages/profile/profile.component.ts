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
  isSaving = signal(false);
  showLogoutConfirm = signal(false);

  ngOnInit() {
    const user = this.authService.currentUser();// Mevcut kullanıcı verisi sadece bir kere çekildi ve null olamaz
    if (!user) { // Dikkkat eğer başka yerde veri güncellemesi yapılır ise bu durumda değişim profile sayfasına kurulan yapı nedeni ile yansımaz.
      this.router.navigate(['/']); // Ancak zaten başka yerden değişim imkanıda tanınmadı.
      return;
    }
    
    this.profileForm = this.fb.group({
      phone: [user?.phone ?? '', [Validators.pattern('^[0-9+ ]*$')]], // Only numbers, plus and space
      address: [user?.address ?? '', [Validators.maxLength(500)]]
    });
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
          // Update form with new values (resets dirty state essentially)
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
