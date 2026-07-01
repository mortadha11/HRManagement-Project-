import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';

export interface Department {
  id: number;
  name: string;
  description?: string | null;
}

export interface Employee {
  id: number;
  firstName: string;
  lastName: string;
  fullName?: string;
  email: string;
  phone?: string | null;
  hireDate: string;
  salary?: number | null;
  isActive: boolean;
  createdAt?: string;
  jobTitle?: string | null;
  jobLevel?: string | null;
  departmentId?: number | null;
  departmentName?: string | null;
  department?: Department | null;
  managerId?: number | null;
  managerName?: string | null;
  role?: string;
  username?: string | null;
  subordinatesCount?: number;
  subordinates?: { id: number, fullName: string, jobTitle?: string }[];
}

export interface UserAccount {
  id: number;
  username: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string | null;
  hasPassword: boolean;
}

export interface Contract {
  id: number;
  employeeId: number;
  employeeName?: string;
  type: string;
  startDate: string;
  endDate?: string | null;
  salary?: number | null;
  position?: string | null;
  workingHours?: number | null;
  status: string; // Active, Expired, Terminated
  createdAt?: string;
}

export interface Leave {
  id: number;
  employeeId: number;
  employeeName?: string;
  type: string;
  startDate: string;
  endDate: string;
  daysRequested: number;
  status: string;
  reason?: string | null;
  moderatedAt?: string | null;
  moderatedById?: number | null;
  moderatorName?: string | null;
  createdAt?: string;
}

export interface EmployeePayload {
  id?: number;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string | null;
  hireDate: string;
  salary?: number | null;
  departmentId?: number | null;
  jobTitle?: string | null;
  jobLevel?: string | null;
  managerId?: number | null;
  role?: string | null;
  isActive?: boolean;
  createdAt?: string;
  password?: string | null;
}

export interface CreateEmployeeResponse {
  employeeId: number;
  firstName: string;
  lastName: string;
  email: string;
  username: string;
  temporaryPassword: string;
  role: string;
}

export interface ResetPasswordResponse {
  employeeId: number;
  username: string;
  temporaryPassword: string;
  message: string;
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

export interface EmployeeTask {
  id: number;
  title: string;
  description: string | null;
  status: string;
  dueDate: string | null;
  createdAt: string;
  employeeId: number;
  managerId: number;
  assigneeName?: string;
  managerName?: string;
  priorityLevel?: string;
}

export interface CreateTaskRequest {
  title: string;
  description?: string | null;
  dueDate?: string | null;
  employeeId: number;
  priorityLevel?: string | null;
}

export interface UpdateTaskRequest {
  title?: string;
  description?: string | null;
  dueDate?: string | null;
  status?: string;
  priorityLevel?: string | null;
}

@Injectable({ providedIn: 'root' })
export class HrApi {
  private readonly base = 'http://localhost:5037/api';

  constructor(private http: HttpClient, private auth: AuthService) { }

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

  createDepartment(payload: Department): Observable<void> {
    return this.http.post<void>(`${this.base}/departments`, payload, this.options);
  }

  updateDepartment(id: number, payload: Department): Observable<void> {
    return this.http.put<void>(`${this.base}/departments/${id}`, payload, this.options);
  }

  deleteDepartment(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/departments/${id}`, this.options);
  }

  // ── Contracts ─────────────────────────────────────────
  getContracts(): Observable<Contract[]> {
    return this.http.get<Contract[]>(`${this.base}/contracts`, this.options);
  }
  
  getContract(id: number): Observable<Contract> {
    return this.http.get<Contract>(`${this.base}/contracts/${id}`, this.options);
  }
  
  getEmployeeContracts(employeeId: number): Observable<Contract[]> {
    return this.http.get<Contract[]>(`${this.base}/contracts/employee/${employeeId}`, this.options);
  }
  
  getExpiringContracts(days: number = 30): Observable<Contract[]> {
    return this.http.get<Contract[]>(`${this.base}/contracts/expiring?days=${days}`, this.options);
  }
  
  createContract(payload: any): Observable<any> {
    return this.http.post<any>(`${this.base}/contracts`, payload, this.options);
  }
  
  updateContract(id: number, payload: any): Observable<void> {
    return this.http.put<void>(`${this.base}/contracts/${id}`, payload, this.options);
  }
  
  deleteContract(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/contracts/${id}`, this.options);
  }

  // ── Leaves ────────────────────────────────────────────
  getLeaves(): Observable<Leave[]> {
    return this.http.get<Leave[]>(`${this.base}/leaverequests`, this.options);
  }
  
  getMyLeaves(): Observable<Leave[]> {
    return this.http.get<Leave[]>(`${this.base}/leaverequests/my`, this.options);
  }
  
  createLeave(payload: any): Observable<any> {
    return this.http.post<any>(`${this.base}/leaverequests`, payload, this.options);
  }
  
  updateLeaveStatus(id: number, status: string): Observable<void> {
    return this.http.put<void>(`${this.base}/leaverequests/${id}/status`, { status }, this.options);
  }
  
  deleteLeave(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/leaverequests/${id}`, this.options);
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

  // ── Tasks ──────────────────────────────────────────────────

  getAssignedTasks(): Observable<EmployeeTask[]> {
    return this.http.get<EmployeeTask[]>(`${this.base}/tasks/assigned`, this.options);
  }

  getCreatedTasks(): Observable<EmployeeTask[]> {
    return this.http.get<EmployeeTask[]>(`${this.base}/tasks/created`, this.options);
  }

  createTask(data: CreateTaskRequest): Observable<any> {
    return this.http.post<any>(`${this.base}/tasks`, data, this.options);
  }

  updateTask(taskId: number, data: UpdateTaskRequest): Observable<any> {
    return this.http.put<any>(`${this.base}/tasks/${taskId}`, data, this.options);
  }

  updateTaskStatus(taskId: number, status: string): Observable<any> {
    return this.http.put<any>(`${this.base}/tasks/${taskId}/status`, { status }, this.options);
  }

  deleteTask(taskId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/tasks/${taskId}`, this.options);
  }
}