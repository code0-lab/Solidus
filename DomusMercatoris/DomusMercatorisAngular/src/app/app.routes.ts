import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { ProfileComponent } from './pages/profile/profile.component';
import { authGuard } from './guards/auth.guard';
import { SearchComponent } from './pages/search/search.component';
import { ForbiddenComponent } from './pages/forbidden/forbidden.component';
import { NotFoundComponent } from './pages/not-found/not-found.component';
import { ServerErrorComponent } from './pages/server-error/server-error.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'search', component: SearchComponent },
  { path: 'products/search', component: SearchComponent },
  { path: 'profile', component: ProfileComponent, canActivate: [authGuard] },
  { path: '403', component: ForbiddenComponent },
  { path: '500', component: ServerErrorComponent },
  { path: '**', component: NotFoundComponent }
];
