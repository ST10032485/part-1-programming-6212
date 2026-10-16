# RaceDay API Endpoint Plan

Planned before any API code is written (Part 1). All routes start with `/api/`.

**Roles:** `None` = public, `Any` = any logged in user, `Organiser` = Organiser role only, `Participant` = Participant role only. "(owner)" means the record must belong to the logged in user.

**Authentication:** JWT bearer token returned by the login endpoint.

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates a new account as Organiser or Participant. | None | `{ "firstName", "lastName", "email", "password", "role" }` | 201 Created, user created. 400 Bad Request, invalid data. 409 Conflict, email already used. |
| POST | /api/auth/login | Checks credentials and returns a JWT token. | None | `{ "email", "password" }` | 200 OK, token returned. 401 Unauthorized, wrong email or password. |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the logged in user's profile. | Any | None | 200 OK, profile details. 401 Unauthorized. |
| PUT | /api/users/me | Updates the logged in user's profile details. | Any | `{ "firstName", "lastName", "phone", "dateOfBirth" }` | 200 OK, updated profile. 400 Bad Request. 401 Unauthorized. |
| DELETE | /api/users/me | Deletes the logged in user's account. | Any | None | 204 No Content. 401 Unauthorized. 409 Conflict, user still has enrolments or events. |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/categories | Lists all race categories. | None | None | 200 OK, list of categories. |
| GET | /api/categories/{id} | Returns one category. | None | None | 200 OK, category. 404 Not Found. |
| POST | /api/categories | Creates a new category, either age based (Under 20) or distance based (10km). | Organiser | `{ "categoryName", "distanceKm (optional)", "description" }` | 201 Created. 400 Bad Request. 401 Unauthorized. 403 Forbidden. 409 Conflict, name already exists. |
| PUT | /api/categories/{id} | Updates a category. | Organiser | `{ "categoryName", "distanceKm (optional)", "description" }` | 200 OK, updated category. 400 Bad Request. 403 Forbidden. 404 Not Found. |
| DELETE | /api/categories/{id} | Deletes a category that is not in use. | Organiser | None | 204 No Content. 403 Forbidden. 404 Not Found. 409 Conflict, category used by an event. |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists events. Supports filters `?city=`, `?status=` and `?date=`. | None | None | 200 OK, list of events. |
| GET | /api/events/{id} | Returns one event with its categories. | None | None | 200 OK, event details. 404 Not Found. |
| GET | /api/events/mine | Lists events created by the logged in organiser. | Organiser | None | 200 OK, list of events. 401 Unauthorized. 403 Forbidden. |
| POST | /api/events | Creates a new event. The eventType is Run, Walk or Cycle. | Organiser | `{ "title", "description", "venue", "city", "eventDate", "registrationDeadline", "eventType", "distanceKm" }` | 201 Created, event created. 400 Bad Request. 401 Unauthorized. 403 Forbidden. |
| PUT | /api/events/{id} | Updates an event, including its status. | Organiser (owner) | `{ "title", "description", "venue", "city", "eventDate", "registrationDeadline", "eventType", "distanceKm", "status" }` | 200 OK, updated event. 400 Bad Request. 403 Forbidden. 404 Not Found. |
| DELETE | /api/events/{id} | Deletes an event. | Organiser (owner) | None | 204 No Content. 403 Forbidden. 404 Not Found. 409 Conflict, event has enrolments. |
| POST | /api/events/{id}/categories | Adds a race category to an event with a fee and a limit. | Organiser (owner) | `{ "categoryId", "entryFee", "maxParticipants" }` | 201 Created. 400 Bad Request. 403 Forbidden. 404 Not Found. 409 Conflict, category already added. |
| DELETE | /api/events/{id}/categories/{categoryId} | Removes a category from an event. | Organiser (owner) | None | 204 No Content. 403 Forbidden. 404 Not Found. 409 Conflict, category has enrolments. |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/events/{id}/enrol | Enrols the logged in participant in a category of the event. A bib number is assigned. | Participant | `{ "categoryId" }` | 201 Created, enrolment with bib number. 400 Bad Request, deadline passed. 404 Not Found. 409 Conflict, already enrolled or category full. |
| GET | /api/enrolments/mine | Lists the logged in participant's enrolments. | Participant | None | 200 OK, list of enrolments. 401 Unauthorized. |
| DELETE | /api/enrolments/{id} | Cancels an enrolment. | Participant (owner) | None | 204 No Content. 403 Forbidden. 404 Not Found. |
| GET | /api/events/{id}/enrolments | Lists everyone enrolled in an event. | Organiser (owner) | None | 200 OK, list of enrolments. 403 Forbidden. 404 Not Found. |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/results | Records a result for an enrolment. | Organiser (owner of the event) | `{ "enrolmentId", "finishTime", "position", "status" }` | 201 Created. 400 Bad Request. 403 Forbidden. 404 Not Found, enrolment missing. 409 Conflict, result already exists. |
| PUT | /api/results/{id} | Updates a result. | Organiser (owner of the event) | `{ "finishTime", "position", "status" }` | 200 OK, updated result. 400 Bad Request. 403 Forbidden. 404 Not Found. |
| DELETE | /api/results/{id} | Deletes a result. | Organiser (owner of the event) | None | 204 No Content. 403 Forbidden. 404 Not Found. |
| GET | /api/events/{id}/results | Returns the results of an event, ordered by category and position. | None | None | 200 OK, list of results. 404 Not Found. |
| GET | /api/results/mine | Returns the logged in participant's results. | Participant | None | 200 OK, list of results. 401 Unauthorized. |
