import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class BlacklistService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/blacklist`;

  blockCompany(companyId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/block-company/${companyId}`, {});
  }

  unblockCompany(companyId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/unblock-company/${companyId}`, {});
  }
}
