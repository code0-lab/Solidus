import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {
  authService = inject(AuthService);
  router = inject(Router);

  phone = '';
  address = '';
  isSaving = false;

  ngOnInit() {
    const user = this.authService.currentUser();
    this.phone = user?.phone ?? '';
    this.address = user?.address ?? '';
  }

  saveProfile() {
    if (!this.authService.currentUser()) {
      return;
    }
    this.isSaving = true;
    const payload = {
      phone: this.phone.trim() ? this.phone.trim() : null,
      address: this.address.trim() ? this.address.trim() : null
    };
    this.authService.updateProfile(payload).subscribe({
      next: profile => {
        this.phone = profile.phone ?? '';
        this.address = profile.address ?? '';
        this.isSaving = false;
        alert('Profile updated successfully.');
      },
      error: () => {
        this.isSaving = false;
        alert('Failed to update profile.');
      }
    });
  }

  logout() {
    if (confirm('Are you sure you want to log out?')) {
      this.authService.logout();
      this.router.navigate(['/']);
    }
  }
}
