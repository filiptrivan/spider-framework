import { Observable } from 'rxjs';
import { AuthBaseService } from './auth-base.service';

export function appInitializer(
  authService: AuthBaseService
): () => Observable<any> {
  return () => {
    return authService.refreshToken()
  };
}