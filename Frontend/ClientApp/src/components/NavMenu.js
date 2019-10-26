import React, { Component } from 'react';
import { Link, HashRouter } from 'react-router-dom';
import { Glyphicon, Nav, Navbar, NavItem } from 'react-bootstrap';
import { LinkContainer } from 'react-router-bootstrap';
import './NavMenu.css';

export class NavMenu extends Component {
  displayName = NavMenu.name

  render() {
    return (
      <HashRouter>
      <Navbar fixed='top' bg='light'>
        <Navbar.Brand>
          <Link to={'/'}>PicAI</Link>
        </Navbar.Brand>
        <Navbar.Toggle />
        <Navbar.Collapse>
          <Nav>
            <LinkContainer to={'/'} exact>
              <Nav.Link>Home</Nav.Link>
            </LinkContainer>
            <LinkContainer to={'/blogs'}>
              <Nav.Link>Blogs</Nav.Link>
            </LinkContainer>
          </Nav>
        </Navbar.Collapse>
        </Navbar>
        </HashRouter>
    );
  }
}
