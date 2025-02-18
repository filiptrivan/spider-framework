import { Observable } from 'rxjs';
import { AuthBaseService } from './auth-base.service';
import { AuthResult } from '../entities/security-entities';

export function authInitializer(authService: AuthBaseService): () => Observable<AuthResult> {
  return () => {
    return authService.refreshToken();
  };
}