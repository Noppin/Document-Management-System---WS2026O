import { BrowserRouter, Route, Routes } from "react-router-dom";
import { AppShell } from "./components/layout/AppShell";
import { CollectionDetailPlaceholderPage } from "./pages/CollectionDetailPlaceholderPage";
import { CollectionsPage } from "./pages/CollectionsPage";
import { DashboardPage } from "./pages/DashboardPage";
import { DocumentDetailPlaceholderPage } from "./pages/DocumentDetailPlaceholderPage";
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
            element={<DocumentDetailPlaceholderPage />}
          />

          <Route path="/collections" element={<CollectionsPage />} />

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
