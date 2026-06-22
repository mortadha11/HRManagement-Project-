import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// ── Interfaces ────────────────────────────────────────────────────────────────

export interface Department {
  id:          number;
  name:        string;
  description?: string | null;
}

export interface Employee {
  id:             number;
  firstName:      string;
  lastName:       string;
  fullName?:      string;
  email:          string;
  phone?:         string | null;
  hireDate:       string;
  salary?:        number | null;
  isActive:       boolean;
  createdAt?:     string;
  jobTitle?:      string | null;
  jobLevel?:      string | null;
  departmentId?:  number | null;
  departmentName?:string | null;
  department?:    Department | null;
  managerId?:     number | null;
  managerName?:   string | null;
  role?:          string;
}

export interface Contract {
  id:           number;
  employeeId:   number;
  type:         string;
  startDate:    string;
  endDate?:     string | null;
  salary?:      number | null;
  isActive:     boolean;
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
  id?:          number;
  firstName:    string;
  lastName:     string;
  email:        string;
  phone?:       string | null;
  hireDate:     string;
  salary?:      number | null;
  departmentId?:number | null;
  jobTitle?:    string | null;
  jobLevel?:    string | null;
  managerId?:   number | null;
  isActive?:    boolean;
  createdAt?:   string;
}

export interface ProfilePayload {
  email: string;
  phone?: string | null;
}

// ── Service ───────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class HrApi {
  private readonly base = 'http://localhost:5037/api';

  constructor(private http: HttpClient) {}

  // Employees
  getEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(`${this.base}/employees`);
  }

  getEmployeeById(id: number): Observable<Employee> {
    return this.http.get<Employee>(`${this.base}/employees/${id}`);
  }

  createEmployee(payload: EmployeePayload): Observable<Employee> {
    return this.http.post<Employee>(`${this.base}/employees`, payload);
  }

  updateEmployee(id: number, payload: EmployeePayload): Observable<void> {
    return this.http.put<void>(`${this.base}/employees/${id}`, payload);
  }

  updateEmployeeProfile(id: number, payload: ProfilePayload): Observable<void> {
    return this.http.put<void>(`${this.base}/employees/${id}/profile`, payload);
  }

  deleteEmployee(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/employees/${id}`);
  }

  // Departments
  getDepartments(): Observable<Department[]> {
    return this.http.get<Department[]>(`${this.base}/departments`);
  }

  // Contracts
  getContracts(): Observable<Contract[]> {
    return this.http.get<Contract[]>(`${this.base}/contracts`);
  }

  // Leaves
  getLeaves(): Observable<Leave[]> {
    return this.http.get<Leave[]>(`${this.base}/leaves`);
  }
}