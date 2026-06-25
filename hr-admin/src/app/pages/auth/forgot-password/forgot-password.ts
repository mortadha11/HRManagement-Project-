import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HrApi } from '../../../services/hr-api';

type PageState = 'form' | 'loading' | 'success' | 'error';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.scss',
})
export class ForgotPassword {
  username      = '';
  state: PageState = 'form';
  tempPassword: string | null = null;
  errorMessage  = '';

  constructor(
    private api:    HrApi,
    private router: Router,
    private cdr:    ChangeDetectorRef
  ) {}

  submit(): void {
    const u = this.username.trim();
    if (!u) {
      this.errorMessage = 'Please enter your username.';
      this.cdr.detectChanges();
      return;
    }

    this.state        = 'loading';
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.api.forgotPassword(u).subscribe({
      next: (res) => {
        this.tempPassword = res.tempPassword;
        this.state        = 'success';
        this.cdr.detectChanges();   // ← force re-render after async response
      },
      error: (err) => {
        console.error('forgot-password error:', err);
        this.errorMessage = 'Something went wrong. Please try again.';
        this.state        = 'error';
        this.cdr.detectChanges();
      },
    });
  }

  copyPassword(): void {
    if (this.tempPassword) navigator.clipboard.writeText(this.tempPassword);
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  reset(): void {
    this.username     = '';
    this.state        = 'form';
    this.tempPassword = null;
    this.errorMessage = '';
    this.cdr.detectChanges();
  }
}
