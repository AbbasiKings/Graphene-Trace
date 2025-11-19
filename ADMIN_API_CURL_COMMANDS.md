# Admin API - cURL Commands

## Prerequisites
1. First, login as Admin to get JWT token:
```bash
curl -X POST http://localhost:5200/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@graphene-trace.com",
    "password": "Admin@123"
  }'
```

Copy the `token` from the response and use it in `YOUR_JWT_TOKEN` below.

---

## 1. Get Dashboard KPIs
```bash
curl -X GET http://localhost:5200/api/admin/dashboard \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

---

## 2. Get All Users
```bash
curl -X GET http://localhost:5200/api/admin/users \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

---

## 3. Create New User
```bash
# Create a Patient
curl -X POST http://localhost:5200/api/admin/users \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "John Doe",
    "email": "john.doe@example.com",
    "password": "Patient123!",
    "role": 3,
    "assignedClinicianId": "22222222-2222-2222-2222-222222222222"
  }'

# Create a Clinician
curl -X POST http://localhost:5200/api/admin/users \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Dr. Jane Smith",
    "email": "jane.smith@example.com",
    "password": "Clinician123!",
    "role": 2,
    "assignedClinicianId": null
  }'
```

**Note:** Role values: 1=Admin, 2=Clinician, 3=Patient

---

## 4. Update User
```bash
curl -X PUT http://localhost:5200/api/admin/users/USER_ID_HERE \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "John Doe Updated",
    "email": "john.doe.updated@example.com",
    "role": 3,
    "isActive": true,
    "assignedClinicianId": "22222222-2222-2222-2222-222222222222",
    "password": "NewPassword123!"
  }'
```

**Note:** `password` is optional - only include if you want to change it.

---

## 5. Delete/Deactivate User
```bash
curl -X DELETE http://localhost:5200/api/admin/users/USER_ID_HERE \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

**Note:** If user has associated data (patient data, alerts, comments), they will be deactivated (soft delete). Otherwise, they will be permanently deleted.

---

## 6. Get System Audit Logs
```bash
# Get last 100 audit logs (default)
curl -X GET http://localhost:5200/api/admin/audit \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"

# Get last 50 audit logs
curl -X GET "http://localhost:5200/api/admin/audit?limit=50" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

---

## Complete Example Script

```bash
#!/bin/bash

# Step 1: Login as Admin
echo "Step 1: Logging in as Admin..."
LOGIN_RESPONSE=$(curl -s -X POST http://localhost:5200/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@graphene-trace.com",
    "password": "Admin123!"
  }')

# Extract token (requires jq or manual parsing)
TOKEN=$(echo $LOGIN_RESPONSE | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo "Login failed!"
  exit 1
fi

echo "Token received: ${TOKEN:0:20}..."

# Step 2: Get Dashboard KPIs
echo -e "\nStep 2: Getting Dashboard KPIs..."
curl -X GET http://localhost:5200/api/admin/dashboard \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"

# Step 3: Get All Users
echo -e "\n\nStep 3: Getting All Users..."
curl -X GET http://localhost:5200/api/admin/users \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"

# Step 4: Create New User
echo -e "\n\nStep 4: Creating New User..."
curl -X POST http://localhost:5200/api/admin/users \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Test Patient",
    "email": "test.patient@example.com",
    "password": "Test123!",
    "role": 3,
    "assignedClinicianId": "22222222-2222-2222-2222-222222222222"
  }'

# Step 5: Get Audit Logs
echo -e "\n\nStep 5: Getting Audit Logs..."
curl -X GET "http://localhost:5200/api/admin/audit?limit=10" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"

echo -e "\n\nDone!"
```

---

## Using with jq (for better JSON formatting)

```bash
# Install jq if needed: brew install jq (macOS) or apt-get install jq (Linux)

# Login and get token
TOKEN=$(curl -s -X POST http://localhost:5200/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@graphene-trace.com","password":"Admin@123"}' \
  | jq -r '.token')

# Get Dashboard (formatted)
curl -s -X GET http://localhost:5200/api/admin/dashboard \
  -H "Authorization: Bearer $TOKEN" \
  | jq '.'

# Get Users (formatted)
curl -s -X GET http://localhost:5200/api/admin/users \
  -H "Authorization: Bearer $TOKEN" \
  | jq '.'
```

---

## Error Responses

- **401 Unauthorized**: Invalid or missing JWT token
- **403 Forbidden**: User is not an Admin
- **404 Not Found**: User ID not found
- **409 Conflict**: Email already exists (on create/update)

