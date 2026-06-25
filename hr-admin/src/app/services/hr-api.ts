import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';

export interface Department {
  id:           number;
  name:         string;
  description?: string | null;
}

export interface Employee {
  id:              number;
  firstName:       string;
  lastName:        string;
  fullName?:       string;
  email:           string;
  phone?:          string | null;
  hireDate:        string;
  salary?:         number | null;
  isActive:        boolean;
  createdAt?:      string;
  jobTitle?:       string | null;
  jobLevel?:       string | null;
  departmentId?:   number | null;
  departmentName?: string | null;
  department?:     Department | null;
  managerId?:      number | null;
  managerName?:    string | null;
  role?:           string;
  username?:       string | null;
}

export interface UserAccount {
  id:           number;
  username:     string;
  role:         string;
  isActive:     boolean;
  createdAt:    string;
  lastLoginAt?: string | null;
  hasPassword:  boolean;
}

export interface Contract {
  id:         number;
  employeeId: number;
  type:       string;
  startDate:  string;
  endDate?:   string | null;
  salary?:    number | null;
  isActive:   boolean;
}

export interface Leave {
  id:         number;
  employeeId: number;
  type:       string;
  startDate:  string;
  endDate:    string;
  status:     string;
  reason?:    string | null;
  employee?:  Employee | null;
}

export interface EmployeePayload {
  id?:           number;
  firstName:     string;
  lastName:      string;
  email:         string;
  phone?:        string | null;
  hireDate:      string;
  salary?:       number | null;
  departmentId?: number | null;
  jobTitle?:     string | null;
  jobLevel?:     string | null;
  managerId?:    number | null;
  role?:         string | null;
  isActive?:     boolean;
  createdAt?:    string;
  password?:     string | null;
}

export interface CreateEmployeeResponse {
  employeeId:         number;
  firstName:          string;
  lastName:           string;
  email:              string;
  username:           string;
  temporaryPassword:  string;
  role:               string;
}

export interface ResetPasswordResponse {
  employeeId:         number;
  username:           string;
  temporaryPassword:  string;
  message:            string;
}

export interface ProfilePayload {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string | null;
}

export interface ChangePasswordPayload {
  currentPassword: string;
  newPassword: string;
}

@Injectable({ providedIn: 'root' })
export class HrApi {
  private readonly base = 'http://localhost:5037/api';

  constructor(private http: HttpClient, private auth: AuthService) {}

  private get options() {
    return { headers: this.auth.authHeaders };
  }

  // ── Employees ─────────────────────────────────────────
  getEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(`${this.base}/employees`, this.options);
  }

  getEmployeeById(id: number): Observable<Employee> {
    return this.http.get<Employee>(`${this.base}/employees/${id}`, this.options);
  }

  createEmployee(payload: EmployeePayload): Observable<CreateEmployeeResponse> {
    return this.http.post<CreateEmployeeResponse>(`${this.base}/employees`, payload, this.options);
  }

  updateEmployee(id: number, payload: EmployeePayload): Observable<void> {
    return this.http.put<void>(`${this.base}/employees/${id}`, payload, this.options);
  }

  updateEmployeeProfile(id: number, payload: ProfilePayload): Observable<void> {
    return this.http.put<void>(`${this.base}/employees/${id}/profile`, payload, this.options);
  }

  deleteEmployee(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/employees/${id}`, this.options);
  }

  resetEmployeePassword(id: number, newPassword?: string | null): Observable<ResetPasswordResponse> {
    return this.http.post<ResetPasswordResponse>(`${this.base}/employees/${id}/reset-password`, {
      newPassword: newPassword?.trim() || null
    }, this.options);
  }

  // ── Departments ───────────────────────────────────────
  getDepartments(): Observable<Department[]> {
    return this.http.get<Department[]>(`${this.base}/departments`, this.options);
  }

  // ── Contracts ─────────────────────────────────────────
  getContracts(): Observable<Contract[]> {
    return this.http.get<Contract[]>(`${this.base}/contracts`, this.options);
  }

  // ── Leaves ────────────────────────────────────────────
  getLeaves(): Observable<Leave[]> {
    return this.http.get<Leave[]>(`${this.base}/leaves`, this.options);
  }

  // ── Auth / User accounts ──────────────────────────────
  getUserByEmployee(employeeId: number): Observable<UserAccount> {
    return this.http.get<UserAccount>(`${this.base}/auth/user/${employeeId}`, this.options);
  }

  resetPassword(employeeId: number, newPassword: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/auth/reset-password`, {
      employeeId,
      newPassword
    }, this.options);
  }

  createAccount(employeeId: number, username: string, password: string, role: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/auth/create-account`, {
      employeeId, username, password, role
    }, this.options);
  }

  forgotPassword(username: string): Observable<{ message: string; tempPassword: string | null }> {
    return this.http.post<{ message: string; tempPassword: string | null }>(
      `${this.base}/auth/forgot-password`, { username }
    );
  }

  changePassword(payload: ChangePasswordPayload): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/auth/change-password`, payload, this.options);
  }
}
