import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './landing.html',
  styleUrl: './landing.scss'
})
export class LandingComponent implements OnInit {
  constructor(
    private readonly auth: AuthService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    if (!this.auth.isLoggedIn) return;

    const role = this.auth.currentUser?.role;
    if (role === 'Admin' || role === 'Manager') {
      this.router.navigate(['/admin/dashboard'], { replaceUrl: true });
      return;
    }

    this.router.navigate(['/employee/profile'], { replaceUrl: true });
  }
}
