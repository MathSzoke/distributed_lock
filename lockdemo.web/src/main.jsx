import React from 'react'
import { createRoot } from 'react-dom/client'
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import App from './App.jsx'

createRoot(document.getElementById('root')).render(
    <FluentProvider theme={webLightTheme}>
        <App />
    </FluentProvider>,
)
