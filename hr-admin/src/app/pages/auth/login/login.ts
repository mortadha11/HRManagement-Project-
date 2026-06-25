import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent implements OnInit {
  credentials = { username: '', password: '' };
  showPassword = false;
  isLoading = false;
  errorMessage = '';

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    if (this.auth.isLoggedIn) {
      this.goToHome();
      return;
    }

    const reason = this.route.snapshot.queryParamMap.get('reason');
    if (reason === 'sign-in-again') {
      this.errorMessage = 'Please sign in again.';
    }
  }

  onLogin(): void {
    if (!this.credentials.username || !this.credentials.password) {
      this.errorMessage = 'Veuillez remplir tous les champs.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.auth.login(this.credentials.username, this.credentials.password).subscribe({
      next: (user) => {
        this.isLoading = false;
        this.goToHome(user.role);
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Identifiants incorrects.';
      }
    });
  }

  private goToHome(role = this.auth.currentUser?.role): void {
    if (role === 'Admin' || role === 'Manager') {
      this.router.navigate(['/admin/dashboard'], { replaceUrl: true });
    } else {
      this.router.navigate(['/employee/profile'], { replaceUrl: true });
    }
  }
}
