import { Routes } from '@angular/router';
import { Dashboard } from './components/dashboard/dashboard';
import { ManifestCreate } from './components/manifest-create/manifest-create';
import { ManifestDetail } from './components/manifest-detail/manifest-detail';

export const routes: Routes = [
  { path: '', component: Dashboard },
  { path: 'create-manifest', component: ManifestCreate },
  { path: 'manifest/:id', component: ManifestDetail },
  { path: '**', redirectTo: '' }
];
