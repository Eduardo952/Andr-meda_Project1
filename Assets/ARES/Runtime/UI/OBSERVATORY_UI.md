# Telescope presentation

Visual reference: `Assets/References/WhatsApp Image 2026-11-15 at 08.27.14.jpeg`.
The reference describes the observation screen, not the telescope's world mesh.

- `ObservatoryView.cs`: full-screen composition, photo attribution, educational details, unlocked journal days and source links.
- `ObservatoryLens.cs`: circular image mask, subtle reticle and digital zoom (1–3×). Zoom affects only the viewport; source textures remain intact.
- `Resources/ARES/UI/Observatory.uss`: scoped dark/lilac appearance and compact sizing. Native UI Toolkit shapes keep the circle and controls sharp at different resolutions.
- `ApodService` and `DiscoveryJournal`: unchanged image selection, offline fallback, API fetching and mission progression rules.

The main screen matches the reference's visual hierarchy: lens, title, credit, zoom/Observe controls and source buttons. Observe reveals the existing Portuguese learning content and discovery history. Dates are not displayed in this view. Original photo metadata stays in the collection.

Focused optional verification: `ARES > Checks > Telescope screen only`. It uses local APOD images without network calls, captures the normal/detail/compact views and checks zoom, locked future days and close. It does not open task windows or exercise rover controls.
