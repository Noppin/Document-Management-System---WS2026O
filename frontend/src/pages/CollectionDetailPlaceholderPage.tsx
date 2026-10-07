import { Alert, Button, Stack, Typography } from "@mui/material";
import { Link as RouterLink, useParams } from "react-router-dom";

export function CollectionDetailPlaceholderPage() {
  const { id } = useParams();

  return (
    <Stack spacing={3}>
      <div>
        <Typography variant="h4">Collection details</Typography>

        <Typography color="text.secondary">
          Collection route: {id ?? "unknown"}
        </Typography>
      </div>

      <Alert severity="info">
        The collection detail route is ready. The complete collection detail
        functionality will be implemented later on.
      </Alert>

      <Button
        component={RouterLink}
        to="/collections"
        variant="outlined"
        sx={{ alignSelf: "flex-start" }}
      >
        Back to collections
      </Button>
    </Stack>
  );
}
