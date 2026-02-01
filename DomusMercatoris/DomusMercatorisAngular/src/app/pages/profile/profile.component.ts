import { Component, inject, OnInit, ChangeDetectionStrategy, signal, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { AlertService } from '../../services/alert.service';
import { UserService } from '../../services/user.service';
import { BlacklistService } from '../../services/blacklist.service';
import { MyCompanyDto } from '../../models/user.model';
import { debounceTime, distinctUntilChanged, switchMap, catchError } from 'rxjs/operators';
import { Subject, of } from 'rxjs';

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
  route = inject(ActivatedRoute);
  destroyRef = inject(DestroyRef);
  fb = inject(FormBuilder);
  toastService = inject(ToastService);
  alertService = inject(AlertService);
  userService = inject(UserService);
  blacklistService = inject(BlacklistService);

  profileForm!: FormGroup;
  passwordForm!: FormGroup;

  isSaving = signal(false);
  activeTab = signal<'edit' | 'password' | 'companies'>('edit');
  imageLoadFailed = signal(false);

  myCompanies = signal<MyCompanyDto[]>([]);

  ngOnInit() {
    const user = this.authService.currentUser();
    if (!user) {
      this.router.navigate(['/']);
      return;
    }

    this.route.queryParams.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      if (params['tab']) {
        const tab = params['tab'];
        if (['edit', 'password', 'companies'].includes(tab)) {
          this.setActiveTab(tab as 'edit' | 'password' | 'companies');
        }
      }
    });

    this.profileForm = this.fb.group({
      firstName: [user.firstName ?? '', [Validators.required]],
      lastName: [user.lastName ?? '', [Validators.required]],
      email: [user.email, [Validators.required, Validators.email]],
      phone: [user?.phone ?? '', [Validators.pattern('^[0-9+ ]*$')]],
      address: [user?.address ?? '', [Validators.maxLength(500)]],
      currentPassword: [''] // Only required if email changes
    });

    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8), Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/)]],
      confirmNewPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });
  }

  passwordMatchValidator(g: FormGroup) {
    return g.get('newPassword')?.value === g.get('confirmNewPassword')?.value
      ? null : { mismatch: true };
  }

  setActiveTab(tab: 'edit' | 'password' | 'companies') {
    this.activeTab.set(tab);
    if (tab === 'password') this.passwordForm.reset();
    if (tab === 'companies') {
        this.loadMyCompanies();
    }
  }

  loadMyCompanies() {
    this.userService.getMyCompanies().subscribe(data => {
      this.myCompanies.set(data);
    });
  }

  toggleBlock(company: MyCompanyDto) {
    if (company.isBlockedByMe) {
        this.blacklistService.unblockCompany(company.id).subscribe(() => {
            this.toastService.show('Company unblocked', 'success');
            this.loadMyCompanies();
        });
    } else {
        this.alertService.showConfirm(
            `Are you sure you want to block ${company.name}? You will no longer see products from this company.`,
            () => {
                this.blacklistService.blockCompany(company.id).subscribe(() => {
                    this.toastService.show('Company blocked', 'success');
                    this.loadMyCompanies();
                });
            }
        );
    }
  }


  // Helper to check if form value is different from initial user data
  get hasChanges(): boolean {
    const user = this.authService.currentUser();
    if (!user) return false;

    const currentVal = this.profileForm.value;
    const initialPhone = user.phone ?? '';
    const initialAddress = user.address ?? '';
    const initialFirst = user.firstName ?? '';
    const initialLast = user.lastName ?? '';
    const initialEmail = user.email;

    const phoneChanged = (currentVal.phone?.trim() ?? '') !== initialPhone;
    const addressChanged = (currentVal.address?.trim() ?? '') !== initialAddress;
    const firstNameChanged = (currentVal.firstName?.trim() ?? '') !== initialFirst;
    const lastNameChanged = (currentVal.lastName?.trim() ?? '') !== initialLast;
    const emailChanged = (currentVal.email?.trim() ?? '') !== initialEmail;

    return phoneChanged || addressChanged || firstNameChanged || lastNameChanged || emailChanged;
  }

  saveProfile() {
    if (this.profileForm.invalid || !this.hasChanges) {
      return;
    }

    const formValue = this.profileForm.value;
    const user = this.authService.currentUser();

    // Check if email changed
    if (user && formValue.email !== user.email && !formValue.currentPassword) {
      this.toastService.error('Current password is required to change email.');
      return;
    }

    this.isSaving.set(true);

    const payload = {
      firstName: formValue.firstName?.trim(),
      lastName: formValue.lastName?.trim(),
      email: formValue.email?.trim(),
      phone: formValue.phone?.trim() || null,
      address: formValue.address?.trim() || null,
      currentPassword: formValue.currentPassword || null
    };

    this.authService.updateProfile(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((profile) => {
          // Update form with new values (clearing password)
          this.profileForm.patchValue({
            currentPassword: ''
          });
          // Update initial state signals handled by auth service
          this.isSaving.set(false);
          this.toastService.success('Profile updated successfully.');
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
          this.setActiveTab('edit');
        },
        error: (err) => {
          this.isSaving.set(false);
          this.toastService.error(err.error?.message || 'Failed to change password.');
        }
      });
  }
// Bunun güvenlik sorunu yaratabileceği göz ardı edilmemeli
  generateRandomPassword() {
    const length = 12;
    const lowers = "abcdefghijklmnopqrstuvwxyz";
    const uppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const digits = "0123456789";
    const specials = "@$!%*?&";
    const all = lowers + uppers + digits + specials;

    let password = "";
    // Ensure one of each required type
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

    this.passwordForm.patchValue({
        newPassword: password,
        confirmNewPassword: password
    });

    // Show password to user
    const input = document.getElementById('newPassword') as HTMLInputElement;
    const confirmInput = document.getElementById('confirmNewPassword') as HTMLInputElement;
    if(input) input.type = 'text';
    if(confirmInput) confirmInput.type = 'text';

    this.toastService.success('Random password generated: ' + password);
  }


  logout() {
    this.alertService.showConfirm(
      'Are you sure you want to log out?',
      () => {
        this.authService.logout();
        this.router.navigate(['/']);
        this.toastService.info('Logged out successfully.');
      }
    );
  }

  onFileSelected(event: any) {
    const file: File = event.target.files[0];
    if (file) {
      this.toastService.info('Uploading...');
      this.authService.uploadProfilePicture(file)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.imageLoadFailed.set(false); // Reset error state on new upload
            this.toastService.success('Profile picture updated successfully.');
          },
          error: (err) => {
            this.toastService.error(err.error || 'Failed to upload profile picture.');
          }
        });
    }
  }

  handleImageError() {
    this.imageLoadFailed.set(true);
  }
}
