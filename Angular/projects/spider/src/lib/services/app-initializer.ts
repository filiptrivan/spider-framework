import { Observable } from 'rxjs';
import { AuthBaseService } from './auth-base.service';

export function appInitializer(
  authService: AuthBaseService,
): () => Promise<Observable<any>> {
  return async () => { // FT: Without async keyword the transloco is loading late
    return authService.refreshToken();
  };
}