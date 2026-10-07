import { BrowserRouter, Route, Routes } from "react-router-dom";
import { AppShell } from "./components/layout/AppShell";
import { CollectionDetailPlaceholderPage } from "./pages/CollectionDetailPlaceholderPage";
import { CollectionsManagementPage } from "./pages/CollectionsManagementPage";
import { DashboardPage } from "./pages/DashboardPage";
import { DocumentDetailPage } from "./pages/DocumentDetailPage";
import { NotFoundPage } from "./pages/NotFoundPage";
import { ValidatedUploadPage } from "./pages/ValidatedUploadPage";

export default function App() {
  return (
    <BrowserRouter>
      <AppShell>
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/upload" element={<ValidatedUploadPage />} />

          <Route
            path="/documents/:id"
            element={<DocumentDetailPage />}
          />

          <Route path="/collections" element={<CollectionsManagementPage />} />

          <Route
            path="/collections/:id"
            element={<CollectionDetailPlaceholderPage />}
          />

          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </AppShell>
    </BrowserRouter>
  );
}
