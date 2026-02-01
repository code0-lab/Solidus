import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { MyCompanyDto } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/users`;

  getMyCompanies(): Observable<MyCompanyDto[]> {
    return this.http.get<MyCompanyDto[]>(`${this.apiUrl}/my-companies`);
  }
}
