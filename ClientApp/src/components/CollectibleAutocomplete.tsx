import React, { useMemo, useCallback } from 'react';
import { Autocomplete, TextField, Box, Typography, CircularProgress } from '@mui/material';
import type { CollectibleObjectDTO } from '../types/CollectibleObjectDTO';

interface CollectibleAutocompleteProps {
    value: CollectibleObjectDTO | null;
    onChange: (value: CollectibleObjectDTO | null) => void;
    collectibles: CollectibleObjectDTO[];
    loading?: boolean;
    error?: string | null;
    disabled?: boolean;
    label?: string;
    placeholder?: string;
    size?: 'small' | 'medium';
}

const CollectibleAutocomplete: React.FC<CollectibleAutocompleteProps> = ({
    value,
    onChange,
    collectibles,
    loading = false,
    error = null,
    disabled = false,
    label = 'Select Item',
    placeholder = 'Search by name, type, or ID...',
    size = 'small',
}) => {
    // Memoize sorted collectibles by name for better UX
    const sortedCollectibles = useMemo(() => {
        return [...collectibles].sort((a, b) => a.name.localeCompare(b.name));
    }, [collectibles]);

    // Optimized filter function with result limiting
    const filterOptions = useCallback((options: CollectibleObjectDTO[], { inputValue }: { inputValue: string }) => {
        const searchTerm = inputValue.toLowerCase().trim();
        
        // If no search term, return limited set for performance
        if (!searchTerm) {
            return options.slice(0, 50);
        }

        // Optimized filtering - limit results for performance
        const matches: CollectibleObjectDTO[] = [];
        const maxResults = 100;

        for (let i = 0; i < options.length && matches.length < maxResults; i++) {
            const option = options[i];
            const lowerName = option.name.toLowerCase();
           

            if (lowerName.startsWith(searchTerm)) {
                matches.push(option);
                continue;
            }
            if (option.id.toString().startsWith(searchTerm)) {
                matches.push(option);
            }
        }

        return matches;
    }, []);

    return (
        <Autocomplete
            value={value}
            onChange={(_, newValue) => onChange(newValue)}
            options={sortedCollectibles}
            getOptionLabel={(option) => option.name}
            isOptionEqualToValue={(option, value) => option.id === value.id}
            loading={loading}
            disabled={disabled || loading}
            size={size}
            filterOptions={filterOptions}
            // Limit rendered options for better performance
            ListboxProps={{
                style: { maxHeight: '400px' }
            }}
            // Only open dropdown when user starts typing or clicks
            openOnFocus={false}
            renderOption={(props, option) => {
                const { key, ...otherProps } = props as any;
                return (
                    <Box component="li" key={`${option.type}-${option.id}`} {...otherProps} sx={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start', py: 1 }}>
                        <Typography variant="body2" sx={{ fontWeight: 500 }}>
                            {option.name}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                            Type: {option.type} • ID: {option.id}
                        </Typography>
                    </Box>
                );
            }}
            renderInput={(params) => (
                <TextField
                    {...params}
                    label={label}
                    placeholder={placeholder}
                    error={!!error}
                    helperText={error || 'Start typing to search...'}
                    InputProps={{
                        ...params.InputProps,
                        endAdornment: (
                            <>
                                {loading ? <CircularProgress color="inherit" size={20} /> : null}
                                {params.InputProps.endAdornment}
                            </>
                        ),
                    }}
                />
            )}
            noOptionsText={loading ? 'Loading collectibles...' : 'No items found - try searching by name'}
        />
    );
};

export default CollectibleAutocomplete;
