import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { Observable, tap } from 'rxjs';

export interface AuthUser {
  token: string;
  userId: number;
  username: string;
  role: string;
  employeeId: number;
  fullName: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = 'http://localhost:5037/api/auth';
  private readonly storageKey = 'hruser';

  constructor(
    private http: HttpClient,
    @Inject(PLATFORM_ID) private platformId: object
  ) {}

  login(username: string, password: string): Observable<AuthUser> {
    return this.http.post<AuthUser>(`${this.apiUrl}/login`, { username, password }).pipe(
      tap(user => {
        if (isPlatformBrowser(this.platformId))
          sessionStorage.setItem(this.storageKey, JSON.stringify(user));
      })
    );
  }

  logout(): void {
    if (isPlatformBrowser(this.platformId))
      sessionStorage.removeItem(this.storageKey);
  }

  clearSession(): void {
    if (isPlatformBrowser(this.platformId))
      sessionStorage.removeItem(this.storageKey);
  }

  updateSessionUser(patch: Partial<AuthUser>): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const current = this.currentUser;
    if (!current) return;
    sessionStorage.setItem(this.storageKey, JSON.stringify({ ...current, ...patch }));
  }

  get currentUser(): AuthUser | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    const raw = sessionStorage.getItem(this.storageKey);
    return raw ? JSON.parse(raw) : null;
  }

  get isLoggedIn(): boolean { return !!this.currentUser; }
  get isAdmin(): boolean    { return this.currentUser?.role === 'Admin'; }
  get isManager(): boolean  { return this.currentUser?.role === 'Manager'; }

  // Helper to build auth headers for protected API calls
  get authHeaders(): HttpHeaders {
    return new HttpHeaders({
      Authorization: `Bearer ${this.currentUser?.token ?? ''}`
    });
  }
}
