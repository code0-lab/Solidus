export const ValidationConstants = {
  password: {
    // Regex for minimum 6 characters
    regex: /^.{6,}$/,
    errorMessage: 'Password must be at least 6 characters long.',
    minLength: 6,
    maxLength: 200
  }
};
