import { Component, Inject, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent {
  credentials  = { username: '', password: '' };
  showPassword = false;
  isLoading    = false;
  errorMessage = '';

  constructor(
    private router: Router,
    private auth:   AuthService,
    @Inject(PLATFORM_ID) private platformId: object
  ) {}

  onLogin() {
    if (!isPlatformBrowser(this.platformId)) return;  // ← SSR fix

    if (!this.credentials.username || !this.credentials.password) {
      this.errorMessage = 'Veuillez remplir tous les champs.';
      return;
    }

    this.isLoading    = true;
    this.errorMessage = '';

    this.auth.login(this.credentials.username, this.credentials.password).subscribe({
      next: (user) => {
        console.log('Login success:', user);
        this.isLoading = false;
        if (user.role === 'Employee') {
          this.router.navigate(['/employee/profile']);
        } else {
          this.router.navigate(['/admin/dashboard']);
        }
      },
      error: (err) => {
        console.error('Login error:', err);
        this.isLoading    = false;
        this.errorMessage = 'Identifiants incorrects.';
      }
    });
  }

  togglePassword() {
    this.showPassword = !this.showPassword;
  }
}